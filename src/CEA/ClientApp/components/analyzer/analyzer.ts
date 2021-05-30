import Vue from 'vue';
import { Component } from 'vue-property-decorator';

interface AnalysisResult {
    text: string;
    codeBig5: string;
    codeGb2312: string;
    codeUcs2: string;
    codeUtf8: string;
    codeUtf16: string;
    charsBig5: string[][];
    charsGb2312: string[][];
    charsUcs2: string[][];
    charsUtf8: string[][];
    charsUtf16: string[][];
    charsOrig: string[][];
    pvwBig5: string;
    pvwGb2312: string;
    pvwUcs2: string;
    pvwUtf8: string;
    pvwUtf16: string;    
    ueBig5: string;
    ueUnicode: string;
    ueUtf8: string;
    codeNcr: string;
    mailSubject: string;
    errorMessage: string;
}

//https://github.com/vuejs/vue-class-component
@Component({
    components: {
        BytesDisp: require('../bytesdisp/bytesdisp.vue.html')
    }
})
export default class AnalyzerView extends Vue {

    currentcount: number = 0;

    text: string = "黑暗執行緒";
    codeBig5: string = "";
    codeGb2312: string = "";
    codeUcs2: string = "";
    codeUtf8: string = "";
    codeUtf16: string = "";
    charsBig5: string[][] = [];
    charsGb2312: string[][] = [];
    charsUcs2: string[][] = [];
    charsUtf8: string[][] = [];
    charsUtf16: string[][] = [];
    charsOrig: string[][] = [];

    pvwBig5: string = "";
    pvwGb2312: string = "";
    pvwUcs2: string = "";
    pvwUtf8: string = "";
    pvwUtf16: string = "";
    ueBig5: string = "";
    ueUnicode: string = "";
    ueUtf8: string = "";
    codeNcr: string = "";
    mailSubject: string = "=?big5?q?=B6=C2=B7=74=B0=F5=A6=E6=BA=FC?=";

    injectProperties(data: any) {
        for (var p in data) (<any>this)[p] = data[p];
    }

    private serializeForm(data: any) {
        return Object.keys(data).map(key => 
            `${encodeURIComponent(key)}=${encodeURIComponent(data[key])}`
        ).join('&');
    }

    private postForm(url: string, data: any) {
        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8'
            },
            body: this.serializeForm(data)
        })
        .then(resp => resp.json() as Promise<AnalysisResult>)
        .then(data => {
            if (data.errorMessage) {
                alert(data.errorMessage);
            }
            else {
                document.body.className = "";
                this.injectProperties(data);
            }
        });
    }

    encodeText() {
        this.postForm('api/Analyzer/EncodeText', { text: this.text });
    }

    private decodeUrlEncode(code: string, encType: string) {
        this.postForm('api/Analyzer/DecodeUrlEncode', { code: code, encType: encType });
    }

    decodeUrlEnc(encType: string) {
        this.decodeUrlEncode((<any>this.$data)["ue" + encType], encType.toLowerCase());
    }


    decodeNcr() {
        this.decodeUrlEncode(this.codeNcr, "ncr");
    }

    decodeMailSubject() {
        this.postForm('api/Analyzer/DecodeMailSubject', { code: this.mailSubject });
    }

    mounted() {
        
    }
}
