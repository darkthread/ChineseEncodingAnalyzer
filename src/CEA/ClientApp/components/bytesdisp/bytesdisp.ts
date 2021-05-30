import Vue from 'vue';

export default {
    props: {
        bytes: {
            type: Array,
            required: true
        },
        title: {
            type: String
        }
    },
    methods: {
        focusCharIndex(i: number) {
            var cssList = document.body.classList;
            var self: any = this;
            for (let i = 0; i < self.bytes.length; i++) {
                cssList.remove('f-' + i);
            }
            cssList.add('f-' + i);
        }
    }
}